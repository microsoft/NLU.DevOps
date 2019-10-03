// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Set magic flag to avoid init on azure-pipelines-task-lib, see:
//    https://github.com/microsoft/azure-pipelines-task-lib/blob/dd18c6a/node/task.ts#L2078
const taskLoadedKey = "_vsts_task_lib_loaded";
global[taskLoadedKey] = true;

import * as tl from "azure-pipelines-task-lib/task";
import * as tr from "azure-pipelines-task-lib/toolrunner";
import { expect } from "chai";
import * as utilities from "nlu-devops-common/utilities";
import * as sinon from "sinon";
import { ImportMock, MockManager } from "ts-mock-imports";
import { run } from "../runTask";

describe("NLUClean", () => {
    let getInputStub: sinon.SinonStub<any[], any>;
    let setResultStub: sinon.SinonStub<any[], any>;

    before(() => {
        // stub task library
        getInputStub = ImportMock.mockFunction(tl, "getInput");
        setResultStub = ImportMock.mockFunction(tl, "setResult");
    });

    after(() => {
        // restore original behavior
        getInputStub.restore();
        setResultStub.restore();
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

        // restore tl.tool method
        toolStub.restore();
    });

    it("uses expected command options", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);

        // stub inputs
        const service = "foo";
        getInputStub.withArgs("service").returns(service);
        const includePath = "bar";
        getInputStub.withArgs("includePath").returns(includePath);
        // run test
        await run();

        // assert calls
        const calls = argMock.getCalls();
        expect(calls.length).to.equal(7);

        // exec dotnet-nlu call
        expect(calls[0].calledWith("clean")).to.be.ok;
        expect(calls[1].calledWith("-s")).to.be.ok;
        expect(calls[2].calledWith(service)).to.be.ok;
        expect(calls[3].calledWith("-a")).to.be.ok;
        expect(calls[4].calledWith("-v")).to.be.ok;
        expect(calls[5].calledWith("-i")).to.be.ok;
        expect(calls[6].calledWith(includePath)).to.be.ok;
    });

    it("sets failed result if command fails", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(1);

        // stub inputs
        const service = "foo";
        getInputStub.withArgs("service").returns(service);

        await run();

        setResultStub.calledWith(tl.TaskResult.Failed);
    });
});
