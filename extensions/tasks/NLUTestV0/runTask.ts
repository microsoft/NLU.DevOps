// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as tl from "azure-pipelines-task-lib/task";
import * as tr from "azure-pipelines-task-lib/toolrunner";
import { writeFileSync } from "fs";
import { getNLUToolRunner } from "nlu-devops-common/utilities";
import * as path from "path";
import {
    downloadStatisticsFromBranch,
    downloadStatisticsFromBuildId,
    getBuildStatistics,
} from "./artifacts";

export async function run() {
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

    const testSettings = tl.getInput("testSettings");
    if (testSettings) {
        tool.arg("-t")
            .arg(testSettings);
    }

    const baseline = await downloadBaselineStatistics();
    if (baseline) {
        tool.arg("-b")
            .arg(baseline);
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

    tl.addAttachment("nlu.devops", "metadata.json", path.join(compareOutput, `metadata.json`));

    const statisticsPath = path.join(compareOutput, "statistics.json");
    const allStatisticsPath = path.join(compareOutput, "allStatistics.json");
    const buildStatistics = await getBuildStatistics(statisticsPath);
    writeFileSync(allStatisticsPath, JSON.stringify(buildStatistics, null, 2));
    tl.addAttachment("nlu.devops", "statistics.json", allStatisticsPath);

    const publishData = {
        artifactname: "statistics",
        artifacttype: "container",
        containerfolder: "statistics",
        localpath: statisticsPath,
    };

    tl.command("artifact.upload", publishData, statisticsPath);
    tl.addBuildTag("nlu.devops.statistics");
}

async function downloadBaselineStatistics() {
    const buildType = tl.getInput("baselineBuildType");
    if (buildType === "specific") {
        const buildIdInput = tl.getInput("baselineBuildId");
        const buildId = parseInt(buildIdInput, 10);
        if (Number.isNaN(buildId)) {
            throw new Error("Must specify a valid build ID in 'baselineBuildId' input.");
        }

        return await downloadStatisticsFromBuildId(buildId);
    }

    const branchName = tl.getInput("baselineBranchName") || undefined;
    if (buildType === "latestFromBranch" && !branchName) {
        throw new Error("Must specify a branch name in 'baselineBranchName'.");
    }

    const results = await downloadStatisticsFromBranch(1, branchName);
    return results.length && results[0].path;
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
        return path.join(tl.getVariable("Agent.TempDirectory"), ".nlu");
    } else {
        return null;
    }
}
