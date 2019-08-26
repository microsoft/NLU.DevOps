// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { IBuildApi } from "azure-devops-node-api/BuildApi";
import { BuildResult, BuildStatus } from "azure-devops-node-api/interfaces/BuildInterfaces";
import { getHandlerFromToken, WebApi } from "azure-devops-node-api/WebApi";
import * as tl from "azure-pipelines-task-lib/task";
import * as tr from "azure-pipelines-task-lib/toolrunner";
import * as fs from "fs";
import { getNLUToolRunner } from "nlu-devops-common/utilities";
import * as path from "path";
import * as unzip from "unzip-stream";

async function run() {
    try {
        const output = getOutputPath();
        await runNLUTest(output);
        await runNLUCompare(output);
        publishTestResults();
        await publishNLUResults();
    } catch (error) {
        tl.setResult(tl.TaskResult.Failed, (error as Error).message);
    }
}

async function runNLUTest(output): Promise<any> {
    const tool = await getNLUToolRunner();
    tool.arg("test")
        .arg("-s")
        .arg(tl.getInput("service"))
        .arg("-u")
        .arg(tl.getInput("utterances"))
        .arg("-o")
        .arg(output)
        .arg("-v");

    const modelSettings = tl.getInput("modelSettings");
    if (modelSettings) {
        tool.arg("-m")
            .arg(modelSettings);
    }

    if (tl.getBoolInput("speech")) {
        tool.arg("--speech");
    }

    const speechDirectory = tl.getInput("speechDirectory");
    if (speechDirectory) {
        tool.arg("-d")
            .arg(speechDirectory);
    }

    const includePath = tl.getInput("includePath");
    if (includePath) {
        tool.arg("-i")
            .arg(includePath);
    }

    let isError = false;
    tool.on("stderr", () => {
        isError = true;
    });

    const result = await tool.exec({
        cwd: tl.getInput("workingDirectory"),
        errStream: process.stderr,
        failOnStdErr: false,
        ignoreReturnCode: true,
        outStream: process.stdout,
        windowsVerbatimArguments: true,
    } as unknown as tr.IExecOptions);

    if (result !== 0 || isError) {
        throw new Error("NLU.DevOps test command failed.");
    }
}

async function runNLUCompare(output): Promise<any> {
    // Only run 'compare' command if 'compareOutput' or is set.
    const compareOutput = getCompareOutputPath();
    if (!compareOutput) {
        return;
    }

    const tool = await getNLUToolRunner();
    tool.arg("compare")
        .arg("-e")
        .arg(tl.getInput("utterances"))
        .arg("-a")
        .arg(output)
        .arg("-o")
        .arg(compareOutput);

    if (tl.getBoolInput("speech")) {
        tool.arg("-t");
    }

    const publishNLUResultsInput = tl.getBoolInput("publishNLUResults");
    if (publishNLUResultsInput) {
        tool.arg("-m");
    }

    let isError = false;
    tool.on("stderr", () => {
        isError = true;
    });

    const result = await tool.exec({
        cwd: tl.getInput("workingDirectory"),
        errStream: process.stderr,
        failOnStdErr: false,
        ignoreReturnCode: true,
        outStream: process.stdout,
        windowsVerbatimArguments: true,
    } as unknown as tr.IExecOptions);

    if (result !== 0 || isError) {
        throw new Error("NLU.DevOps compare command failed.");
    }
}

function publishTestResults() {
    if (!tl.getBoolInput("publishTestResults")) {
        return;
    }

    const compareOutput = getCompareOutputPath() as string;

    // Sending allowBrokenSymbolicLinks as true, so we don't want to throw error when symlinks are broken.
    // And can continue with other files if there are any.
    const findOptions = {
        allowBrokenSymbolicLinks: true,
        followSpecifiedSymbolicLink: true,
        followSymbolicLinks: true,
    } as tl.FindOptions;

    const resultFiles = tl.findMatch(compareOutput, ["**/TestResult.xml"], findOptions);

    if (!resultFiles || resultFiles.length === 0) {
        tl.warning("No test result files matching 'TestResult.xml' were found.");
    } else {
        const properties = {
            resultFiles,
            testRunSystem: "VSTS - NLU.DevOps",
            type: "NUnit",
        };

        tl.command("results.publish", properties, "");
    }
}

async function publishNLUResults() {
    if (!tl.getBoolInput("publishNLUResults")) {
        return;
    }

    const compareOutput = getCompareOutputPath() as string;

    console.log("Publishing metadata attachment for NLU results.");
    tl.addAttachment("nlu.devops", "metadata", path.join(compareOutput, `metadata.json`));

    console.log("Publishing statistics attachment for NLU results.");
    const statisticsPath = path.join(compareOutput, "statistics.json");
    const allStatisticsPath = path.join(compareOutput, "allStatistics.json");
    const buildStatistics = await getBuildStatistics(statisticsPath);
    fs.writeFileSync(allStatisticsPath, JSON.stringify(buildStatistics));
    tl.addAttachment("nlu.devops", "statistics", allStatisticsPath);

    if (tl.getVariable("Build.SourceBranch") === "refs/heads/master") {
        const publishData = {
            artifactname: "statistics",
            artifacttype: "container",
            containerfolder: "statistics",
            localpath: statisticsPath,
        };

        tl.command("artifact.upload", publishData, statisticsPath);
        tl.addBuildTag("nlu.devops.statistics");
    }
}

async function getBuildStatistics(statisticsPath: string): Promise<Array<{ id: string, statistics: any }>> {
    const buildStatistics = await getPreviousBuildStatistics();
    const statisticsData = fs.readFileSync(statisticsPath).toString().trim();
    const statistics = JSON.parse(statisticsData);
    buildStatistics.push({
        id: tl.getVariable("Build.BuildId"),
        statistics,
    });

    return buildStatistics;
}

async function getPreviousBuildStatistics(): Promise<Array<{ id: string, statistics: any }>> {
    const compareBuildCountInput = tl.getInput("compareBuildCount");
    const compareBuildCount = parseInt(compareBuildCountInput, 10);
    if (Number.isNaN(compareBuildCount)) {
        throw new Error("Input value for 'compareBuildCount' must be a valid integer.");
    }

    if (!compareBuildCount) {
        return [];
    }

    const endpointUrl = tl.getVariable("System.TeamFoundationCollectionUri");
    const accessToken = tl.getEndpointAuthorizationParameter("SYSTEMVSSCONNECTION", "AccessToken", false);
    const credentialHandler = getHandlerFromToken(accessToken);
    const webApi = new WebApi(endpointUrl, credentialHandler);
    const buildApi = await webApi.getBuildApi();

    const projectId = tl.getVariable("System.TeamProjectId");
    const definitionId = tl.getVariable("System.DefinitionId");
    const builds = await buildApi.getBuilds(
        projectId, /* project */
        [parseInt(definitionId, 10)], /* definitions */
        undefined, /* queues */
        undefined, /* buildNumber */
        undefined, /* minTime */
        undefined, /* maxTime */
        undefined, /* requestedFor */
        undefined, /* reasonFilter */
        BuildStatus.Completed, /* statusFilter */
        BuildResult.Succeeded, /* resultFilter */
        ["nlu.devops.statistics"], /* tagFilters */
        undefined, /* properties */
        compareBuildCount, /* top */
        undefined, /* continuationToken */
        undefined, /* maxBUildsPerDefinition */
        undefined, /* deletedFilter */
        undefined, /* queryOrder */
        "refs/heads/master" /* branchName */);

    console.log(`Found previous builds: ${builds.map((build) => build.id).join(", ")}`);
    const artifactPromises = builds.map((build) =>
        downloadStatisticsArtifact(projectId, buildApi, build.id as number));
    return await Promise.all(artifactPromises);
}

async function downloadStatisticsArtifact(projectId: string, client: IBuildApi, buildId: number):
    Promise<{ id: string, statistics: any }> {
        const artifactStream = await client.getArtifactContentZip(projectId, buildId, "statistics");
        const id = `${buildId}`;
        const unzipPath = path.join(tl.getVariable("Agent.TempDirectory"), "artifacts", id);
        await new Promise((resolve, _) => artifactStream.pipe(unzip.Extract({ path: unzipPath })).on("close", resolve));
        const statisticsData = fs.readFileSync(path.join(unzipPath, "statistics", "statistics.json")).toString().trim();
        const statistics = JSON.parse(statisticsData);
        return { id, statistics };
}

function getOutputPath() {
    const output = tl.getInput("output");
    const compareOutput = getCompareOutputPath();
    if (output) {
        return output;
    } else if (compareOutput) {
        return path.join(compareOutput, "results.json");
    } else {
        throw new Error("Must set 'output' if 'compareOutput'," +
            "'publishTestResults', or 'publishNLUResults' are unused.");
    }
}

function getCompareOutputPath() {
    const compareOutput = tl.getInput("compareOutput");
    const publishTestResultsInput = tl.getBoolInput("publishTestResults");
    const publishNLUResultsInput = tl.getBoolInput("publishNLUResults");
    if (compareOutput) {
        return compareOutput;
    } else if (publishTestResultsInput || publishNLUResultsInput) {
        const outputSubfolder = tl.getBoolInput("speech") ? "speech" : "text";
        return path.join(tl.getVariable("Agent.TempDirectory"), ".nlu", outputSubfolder);
    } else {
        return null;
    }
}

run();
