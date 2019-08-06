import * as tl from "azure-pipelines-task-lib/task";
import * as tr from "azure-pipelines-task-lib/toolrunner";
import { getNLUToolRunner } from "nlu-devops-common/utilities";

async function run() {
    try {
        const tool = await getNLUToolRunner();
        tool.arg("train")
            .arg("-s")
            .arg(tl.getInput("service"))
            .arg("-u")
            .arg(tl.getInput("utterances"))
            .arg("-a")
            .arg("-v");

        const modelSettings = tl.getInput("modelSettings");
        if (modelSettings) {
            tool.arg("-m")
                .arg(modelSettings);
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
