import * as tl from "azure-pipelines-task-lib/task";
import { installDotNetTool } from "nlu-devops-common/utilities";

async function run() {
    try {
        await installDotNetTool(tl.getInput("packageName"));
    } catch (err) {
        tl.setResult(tl.TaskResult.Failed, (err as Error).message);
    }
}

run();
