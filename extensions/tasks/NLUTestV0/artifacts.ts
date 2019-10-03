// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { IBuildApi } from "azure-devops-node-api/BuildApi";
import { BuildResult, BuildStatus } from "azure-devops-node-api/interfaces/BuildInterfaces";
import { getHandlerFromToken, WebApi } from "azure-devops-node-api/WebApi";
import * as tl from "azure-pipelines-task-lib/task";
import { readFileSync } from "fs";
import * as path from "path";
import * as unzip from "unzip-stream";

export async function getBuildStatistics(statisticsPath: string): Promise<Array<{ id: string, statistics: any }>> {
    const buildStatistics = await getPreviousBuildStatistics();
    const statisticsData = readFileSync(statisticsPath).toString().trim();
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
        const statisticsData = readFileSync(path.join(unzipPath, "statistics", "statistics.json")).toString().trim();
        const statistics = JSON.parse(statisticsData);
        return { id, statistics };
}
