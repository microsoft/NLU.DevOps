import * as tl from "azure-pipelines-task-lib/task";
import * as tr from "azure-pipelines-task-lib/toolrunner";
import { getNLUToolRunner } from "nlu-devops-common/utilities";
import * as path from "path";

async function run() {
    try {
        const output = getOutputPath();
        await runNLUTest(output);
        await runNLUCompare(output);
        publishTestResults();
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

    const publishNLUResults = tl.getBoolInput("publishNLUResults");
    if (publishNLUResults) {
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
