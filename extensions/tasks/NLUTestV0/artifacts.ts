// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { IBuildApi } from "azure-devops-node-api/BuildApi";
import { BuildResult, BuildStatus } from "azure-devops-node-api/interfaces/BuildInterfaces";
import { getHandlerFromToken, WebApi } from "azure-devops-node-api/WebApi";
import * as tl from "azure-pipelines-task-lib/task";
import { existsSync, readFileSync } from "fs";
import * as path from "path";
import * as unzip from "unzip-stream";

export async function getBuildStatistics(statisticsPath: string) {
    const compareBuildCountInput = tl.getInput("compareBuildCount");
    const compareBuildCount = parseInt(compareBuildCountInput, 10);
    if (Number.isNaN(compareBuildCount)) {
        throw new Error("Input value for 'compareBuildCount' must be a valid integer.");
    }

    const buildStatistics = await downloadStatisticsFromBranch(compareBuildCount, "refs/heads/master");

    const statisticsData = readFileSync(statisticsPath).toString().trim();
    const statistics = JSON.parse(statisticsData);

    return [
        ...buildStatistics.map((item) => {
            return {
                id: item.id,
                statistics: JSON.parse(readFileSync(item.path).toString().trim()),
            };
        }),
        {
            id: tl.getVariable("Build.BuildId"),
            statistics,
        },
    ];
}

export async function downloadStatisticsFromBranch(count: number, branchName?: string) {
    if (!count) {
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
        count, /* top */
        undefined, /* continuationToken */
        undefined, /* maxBuildsPerDefinition */
        undefined, /* deletedFilter */
        undefined, /* queryOrder */
        branchName /* branchName */);

    console.log(`Found previous builds: ${builds.map((build) => build.id).join(", ")}`);
    const artifactPromises = builds.map(async (build) => {
        const statisticsPath = await downloadStatisticsArtifact(projectId, buildApi, build.id as number);
        return {
            id: `${build.id}`,
            path: statisticsPath,
        };
    });

    return await Promise.all(artifactPromises);
}

export async function downloadStatisticsFromBuildId(buildId: number) {
    const endpointUrl = tl.getVariable("System.TeamFoundationCollectionUri");
    const accessToken = tl.getEndpointAuthorizationParameter("SYSTEMVSSCONNECTION", "AccessToken", false);
    const credentialHandler = getHandlerFromToken(accessToken);
    const webApi = new WebApi(endpointUrl, credentialHandler);
    const buildApi = await webApi.getBuildApi();
    const projectId = tl.getVariable("System.TeamProjectId");
    return downloadStatisticsArtifact(projectId, buildApi, buildId);
}

async function downloadStatisticsArtifact(projectId: string, client: IBuildApi, buildId: number) {
    const unzipPath = path.join(tl.getVariable("Agent.TempDirectory"), "artifacts", `${buildId}`);
    const statisticsPath = path.join(unzipPath, "statistics", "statistics.json");

    if (!existsSync(statisticsPath)) {
        const artifactStream = await client.getArtifactContentZip(projectId, buildId, "statistics");
        await new Promise((resolve, _) => artifactStream.pipe(unzip.Extract({ path: unzipPath })).on("close", resolve));
    }

    return statisticsPath;
}
