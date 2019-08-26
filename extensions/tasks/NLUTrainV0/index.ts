import * as tl from "azure-pipelines-task-lib/task";
import * as tr from "azure-pipelines-task-lib/toolrunner";
import { getNLUToolRunner } from "nlu-devops-common/utilities";

async function run() {
    try {
        const tool = await getNLUToolRunner();
        tool.arg("train")
            .arg("-s")
            .arg(tl.getInput("service"))
            .arg("-a")
            .arg("-v");

        const utterances = tl.getInput("utterances");
        if (utterances) {
            tool.arg("-u")
                .arg(utterances);
        }

        const modelSettings = tl.getInput("modelSettings");
        if (modelSettings) {
            tool.arg("-m")
                .arg(modelSettings);
        }

        const includePath = tl.getInput("includePath");
        if (includePath) {
            tool.arg("-i")
                .arg(includePath);
        }

        if (!utterances && !modelSettings) {
            throw new Error("Must provide either 'utterances' or 'modelSetting' task input.");
        }

        const result = await tool.exec({
            cwd: tl.getInput("workingDirectory"),
            errStream: process.stderr,
            failOnStdErr: true,
            ignoreReturnCode: true,
            outStream: process.stdout,
            windowsVerbatimArguments: true,
        } as unknown as tr.IExecOptions);

        if (result !== 0) {
            throw new Error("NLU.DevOps train command failed.");
        }
    } catch (err) {
        tl.setResult(tl.TaskResult.Failed, (err as Error).message);
    }
}

run();
