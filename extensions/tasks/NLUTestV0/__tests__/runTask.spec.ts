// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Set magic flag to avoid init on azure-pipelines-task-lib, see:
//    https://github.com/microsoft/azure-pipelines-task-lib/blob/dd18c6a/node/task.ts#L2078
const taskLoadedKey = "_vsts_task_lib_loaded";
global[taskLoadedKey] = true;

import * as tl from "azure-pipelines-task-lib/task";
import * as tr from "azure-pipelines-task-lib/toolrunner";
import { expect } from "chai";
import * as fs from "fs";
import * as utilities from "nlu-devops-common/utilities";
import * as path from "path";
import * as sinon from "sinon";
import { ImportMock, MockManager } from "ts-mock-imports";
import * as artifacts from "../artifacts";
import { run } from "../runTask";

describe("NLUTest", () => {
    let getInputStub: sinon.SinonStub<any[], any>;
    let getBoolInputStub: sinon.SinonStub<any[], any>;
    let setResultStub: sinon.SinonStub<any[], any>;
    let findMatchStub: sinon.SinonStub<any[], any>;
    let commandStub: sinon.SinonStub<any[], any>;
    let warningStub: sinon.SinonStub<any[], any>;
    let addAttachmentStub: sinon.SinonStub<any[], any>;
    let addBuildTagStub: sinon.SinonStub<any[], any>;
    let getBuildStatisticsStub: sinon.SinonStub<any[], any>;
    let downloadStatisticsFromBranchStub: sinon.SinonStub<any[], any>;
    let writeFileSyncStub: sinon.SinonStub<any[], any>;
    let getVariableStub: sinon.SinonStub<any[], any>;

    before(() => {
        // stub task library
        getInputStub = ImportMock.mockFunction(tl, "getInput");
        getBoolInputStub = ImportMock.mockFunction(tl, "getBoolInput");
        setResultStub = ImportMock.mockFunction(tl, "setResult");
        findMatchStub = ImportMock.mockFunction(tl, "findMatch");
        commandStub = ImportMock.mockFunction(tl, "command");
        warningStub = ImportMock.mockFunction(tl, "warning");
        addAttachmentStub = ImportMock.mockFunction(tl, "addAttachment");
        addBuildTagStub = ImportMock.mockFunction(tl, "addBuildTag");
        getBuildStatisticsStub = ImportMock.mockFunction(artifacts, "getBuildStatistics");
        downloadStatisticsFromBranchStub = ImportMock.mockFunction(artifacts, "downloadStatisticsFromBranch");
        writeFileSyncStub = ImportMock.mockFunction(fs, "writeFileSync");

        getVariableStub = ImportMock.mockFunction(tl, "getVariable");
        getVariableStub.withArgs("Agent.TempDirectory").returns(".");
    });

    after(() => {
        // restore original behavior
        getInputStub.restore();
        getBoolInputStub.restore();
        setResultStub.restore();
        findMatchStub.restore();
        commandStub.restore();
        warningStub.restore();
        addAttachmentStub.restore();
        addBuildTagStub.restore();
        writeFileSyncStub.restore();
        getBuildStatisticsStub.restore();
        downloadStatisticsFromBranchStub.restore();

        getVariableStub.restore();
    });

    let toolStub: sinon.SinonStub<any[], any>;
    let toolMock: MockManager<tr.ToolRunner>;
    let mockTool: tr.ToolRunner;
    let argMock: sinon.SinonStub<any[], any>;

    beforeEach(() => {
        // create mock ToolRunner instance
        toolMock = ImportMock.mockClass(tr, "ToolRunner");
        mockTool = toolMock.getMockInstance();
        argMock = toolMock.mock("arg", mockTool);

        // mock tl.tool method
        toolStub = ImportMock.mockFunction(utilities, "getNLUToolRunner").returns(mockTool);
    });

    afterEach(() => {
        // reset non-default task stubs
        getInputStub.reset();
        getBoolInputStub.reset();
        setResultStub.reset();
        findMatchStub.reset();
        commandStub.reset();
        warningStub.reset();
        addAttachmentStub.reset();
        addBuildTagStub.reset();
        getBuildStatisticsStub.reset();
        downloadStatisticsFromBranchStub.reset();
        writeFileSyncStub.reset();

        // restore tl.tool method
        toolStub.restore();
    });

    it("only runs test", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);

        // stub inputs
        const service = "foo";
        const utterances = "bar";
        const output = "qux";
        const includePath = "baz";
        getInputStub.withArgs("service").returns(service);
        getInputStub.withArgs("utterances").returns(utterances);
        getInputStub.withArgs("output").returns(output);
        getInputStub.withArgs("includePath").returns(includePath);

        // run test
        await run();

        // assert calls
        const calls = argMock.getCalls();
        expect(calls.length).to.equal(10);

        // exec dotnet-nlu call
        expect(calls[0].calledWith("test")).to.be.ok;
        expect(calls[1].calledWith("-s")).to.be.ok;
        expect(calls[2].calledWith(service)).to.be.ok;
        expect(calls[3].calledWith("-u")).to.be.ok;
        expect(calls[4].calledWith(utterances)).to.be.ok;
        expect(calls[5].calledWith("-o")).to.be.ok;
        expect(calls[6].calledWith(output)).to.be.ok;
        expect(calls[7].calledWith("-v")).to.be.ok;
        expect(calls[8].calledWith("-i")).to.be.ok;
        expect(calls[9].calledWith(includePath)).to.be.ok;
    });

    it("uses optional inputs for test command", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);

        // stub inputs
        const service = "a";
        const utterances = "b";
        const output = "c";
        const modelSettings = "d";
        const speechDirectory = "e";
        getInputStub.withArgs("service").returns(service);
        getInputStub.withArgs("utterances").returns(utterances);
        getInputStub.withArgs("output").returns(output);
        getInputStub.withArgs("modelSettings").returns(modelSettings);
        getInputStub.withArgs("speechDirectory").returns(speechDirectory);

        // run test
        await run();

        // assert calls
        const calls = argMock.getCalls();
        expect(calls.length).to.equal(12);

        // exec dotnet-nlu call
        expect(calls[8].calledWith("-m")).to.be.ok;
        expect(calls[9].calledWith(modelSettings)).to.be.ok;
        expect(calls[10].calledWith("-d")).to.be.ok;
        expect(calls[11].calledWith(speechDirectory)).to.be.ok;
    });

    it("sets failed result if test command fails", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(1);

        // stub inputs
        const service = "foo";
        const utterances = "bar";
        const output = "qux";
        getInputStub.withArgs("service").returns(service);
        getInputStub.withArgs("utterances").returns(utterances);
        getInputStub.withArgs("output").returns(output);

        // run test
        await run();

        // assert result
        expect(setResultStub.calledWith(tl.TaskResult.Failed)).to.be.ok;
    });

    it("publishes test results", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);
        execStub.onCall(1).returns(0);

        // stub inputs
        const service = "foo";
        const utterances = "bar";
        getInputStub.withArgs("service").returns(service);
        getInputStub.withArgs("utterances").returns(utterances);
        getBoolInputStub.withArgs("publishTestResults").returns(true);

        // stub previous build results
        downloadStatisticsFromBranchStub.returns([]);

        // stub test results match
        const resultFiles = [ "foo" ];
        findMatchStub.returns(resultFiles);

        // run test
        await run();

        // assert calls
        const calls = argMock.getCalls();
        expect(calls.length).to.equal(8 /* test */ + 7 /* compare */);

        // exec dotnet-nlu call
        const output = ".nlu";
        const results = path.join(output, "results.json");
        expect(calls[5].calledWith("-o")).to.be.ok;
        expect(calls[6].calledWith(results)).to.be.ok;
        expect(calls[8].calledWith("compare")).to.be.ok;
        expect(calls[9].calledWith("-e")).to.be.ok;
        expect(calls[10].calledWith(utterances)).to.be.ok;
        expect(calls[11].calledWith("-a")).to.be.ok;
        expect(calls[12].calledWith(results)).to.be.ok;
        expect(calls[13].calledWith("-o")).to.be.ok;
        expect(calls[14].calledWith(output)).to.be.ok;

        // assert publish tests
        const publishData = {
            resultFiles,
            testRunSystem: "VSTS - NLU.DevOps",
            type: "NUnit",
        };

        expect(commandStub.calledWith("results.publish", publishData)).to.be.ok;
    });

    it("warns when test results are not generated", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);
        execStub.onCall(1).returns(0);

        // stub inputs
        const service = "foo";
        const utterances = "bar";
        getInputStub.withArgs("service").returns(service);
        getInputStub.withArgs("utterances").returns(utterances);
        getBoolInputStub.withArgs("publishTestResults").returns(true);

        // stub previous build results
        downloadStatisticsFromBranchStub.returns([]);

        // stub test results match
        const resultsFiles = [];
        findMatchStub.returns(resultsFiles);

        // run test
        await run();

        // assert warning called
        expect(warningStub.called);

        // assert publish tests not called
        expect(commandStub.notCalled);
    });

    it("publishes NLU results", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);
        execStub.onCall(1).returns(0);

        // stub inputs
        const service = "foo";
        const utterances = "bar";
        getInputStub.withArgs("service").returns(service);
        getInputStub.withArgs("utterances").returns(utterances);
        getBoolInputStub.withArgs("publishNLUResults").returns(true);

        // stub previous build results
        getBuildStatisticsStub.returns([]);
        downloadStatisticsFromBranchStub.returns([]);

        // run test
        await run();

        // assert calls
        const calls = argMock.getCalls();
        expect(calls.length).to.equal(8 /* test */ + 7 /* compare */);

        // assert statistics written
        const allStatisticsPath = path.join(".nlu", "allStatistics.json");
        expect(writeFileSyncStub.calledWith(allStatisticsPath, "[]")).to.be.ok;

        // assert adds attachment
        const metadataPath = path.join(".nlu", "metadata.json");
        expect(addAttachmentStub.calledWith("nlu.devops", "metadata.json", metadataPath)).to.be.ok;
        expect(addAttachmentStub.calledWith("nlu.devops", "statistics.json", allStatisticsPath)).to.be.ok;
    });

    it("always publishes statistics from any build", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);
        execStub.onCall(1).returns(0);

        // stub inputs
        const service = "foo";
        const utterances = "bar";
        getInputStub.withArgs("service").returns(service);
        getInputStub.withArgs("utterances").returns(utterances);
        getBoolInputStub.withArgs("publishNLUResults").returns(true);

        // stub previous build results
        getBuildStatisticsStub.returns([]);
        downloadStatisticsFromBranchStub.returns([]);

        // run test
        await run();

        // assert publishes artifact
        const statisticsPath = path.join(".nlu", "statistics.json");
        const publishData = {
            artifactname: "statistics",
            artifacttype: "container",
            containerfolder: "statistics",
            localpath: statisticsPath,
        };

        expect(commandStub.calledWith("artifact.upload", publishData, statisticsPath)).to.be.ok;

        // assert tags build
        expect(addBuildTagStub.calledWith("nlu.devops.statistics")).to.be.ok;
    });

    it("uses provided output folder for compare results", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);
        execStub.onCall(1).returns(0);

        // stub inputs
        const service = "foo";
        const utterances = "bar";
        const compareOutput = "compareOutput";
        getInputStub.withArgs("service").returns(service);
        getInputStub.withArgs("utterances").returns(utterances);
        getInputStub.withArgs("compareOutput").returns(compareOutput);

        // stub previous build results
        downloadStatisticsFromBranchStub.returns([]);

        // run test
        await run();

        // assert calls
        const calls = argMock.getCalls();
        expect(calls.length).to.equal(8 /* test */ + 7 /* compare */);

        // exec dotnet-nlu call
        expect(calls[13].calledWith("-o")).to.be.ok;
        expect(calls[14].calledWith(compareOutput)).to.be.ok;
    });

    it("sets failed result if compare command fails", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);
        execStub.onCall(1).returns(1);

        // stub inputs
        const service = "foo";
        const utterances = "bar";
        const compareOutput = "compareOutput";
        getInputStub.withArgs("service").returns(service);
        getInputStub.withArgs("utterances").returns(utterances);
        getInputStub.withArgs("compareOutput").returns(compareOutput);

        // stub previous build results
        downloadStatisticsFromBranchStub.returns([]);

        // run test
        await run();

        // assert result
        expect(setResultStub.calledWith(tl.TaskResult.Failed)).to.be.ok;
    });
});
