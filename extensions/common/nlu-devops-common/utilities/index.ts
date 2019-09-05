// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as tl from "azure-pipelines-task-lib/task";
import * as tr from "azure-pipelines-task-lib/toolrunner";
import * as path from "path";

export async function getNLUToolRunner(): Promise<tr.ToolRunner> {
    const dotnetPath = tl.which("dotnet", false);
    if (!dotnetPath) {
        throw new Error("Please install .NET Core to enable NLU.DevOps tasks.");
    }

    const dotnetVersion = tl.tool(dotnetPath)
        .arg("nlu")
        .arg("--version");

    const result = await dotnetVersion.exec({
        failOnStdErr: false,
        ignoreReturnCode: true,
        windowsVerbatimArguments: true,
    } as unknown as tr.IExecOptions);

    const nupkgPath = tl.getInput("nupkgPath");
    const toolVersion = tl.getInput("toolVersion");
    if (result !== 0 || nupkgPath || toolVersion) {
        const toolPath = path.join(tl.getVariable("Agent.TempDirectory"), ".dotnet");
        if (result === 0) {
            const dotnetUninstall = tl.tool(dotnetPath)
                .arg("tool")
                .arg("uninstall")
                .arg("dotnet-nlu")
                .arg("--tool-path")
                .arg(toolPath);

            let isUninstallError = false;
            dotnetUninstall.on("stderr", () => {
                isUninstallError = true;
            });

            const uninstallResult = await dotnetUninstall.exec({
                errStream: process.stderr,
                failOnStdErr: false,
                ignoreReturnCode: true,
                outStream: process.stdout,
                windowsVerbatimArguments: true,
            } as unknown as tr.IExecOptions);

            if (uninstallResult !== 0 || isUninstallError) {
                throw new Error("Failed to uninstall NLU.DevOps.");
            }
        }

        const dotnetInstall = tl.tool(dotnetPath)
            .arg("tool")
            .arg("install")
            .arg("dotnet-nlu")
            .arg("--tool-path")
            .arg(toolPath);

        if (nupkgPath) {
            dotnetInstall.arg("--add-source")
                .arg(nupkgPath);
        } else if (toolVersion) {
            dotnetInstall.arg("--version")
                .arg(toolVersion);
        }

        let isInstallError = false;
        dotnetInstall.on("stderr", () => {
            isInstallError = true;
        });

        const installResult = await dotnetInstall.exec({
            errStream: process.stderr,
            failOnStdErr: false,
            ignoreReturnCode: true,
            outStream: process.stdout,
            windowsVerbatimArguments: true,
        } as unknown as tr.IExecOptions);

        if (installResult !== 0 || isInstallError) {
            throw new Error("Failed to install NLU.DevOps.");
        }

        process.env.PATH = `${toolPath}${path.delimiter}${process.env.PATH}`;
        tl.prependPath(toolPath);
    }

    return tl.tool(dotnetPath)
        .arg("nlu");
}
