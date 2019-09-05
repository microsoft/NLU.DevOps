// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as tl from "azure-pipelines-task-lib/task";
import * as tr from "azure-pipelines-task-lib/toolrunner";
import { getNLUToolRunner } from "nlu-devops-common/utilities";

async function run() {
    try {
        const tool = await getNLUToolRunner();
        tool.arg("clean")
            .arg("-s")
            .arg(tl.getInput("service"))
            .arg("-a")
            .arg("-v");

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
            throw new Error("NLU.DevOps clean command failed.");
        }
    } catch (err) {
        tl.setResult(tl.TaskResult.Failed, (err as Error).message);
    }
}

run();
