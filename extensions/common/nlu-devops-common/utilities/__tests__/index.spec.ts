// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Set magic flag to avoid init on azure-pipelines-task-lib, see:
//    https://github.com/microsoft/azure-pipelines-task-lib/blob/dd18c6a/node/task.ts#L2078
const taskLoadedKey = "_vsts_task_lib_loaded";
global[taskLoadedKey] = true;

import * as tl from "azure-pipelines-task-lib/task";
import * as tr from "azure-pipelines-task-lib/toolrunner";

import { expect } from "chai";
import * as path from "path";
import * as sinon from "sinon";
import { ImportMock, MockManager } from "ts-mock-imports";
import { getNLUToolRunner } from "../index";

describe("getNLUToolRunner without .NET Core", () => {
    it("throws", (done) => {
        const whichStub = ImportMock.mockFunction(tl, "which");
        whichStub.returns(null);
        getNLUToolRunner()
            .then(() => done(new Error("Expected method to reject.")))
            .catch(() => done());
        whichStub.restore();
    });
});

describe("getNLUToolRunner", () => {
    let getInputStub: sinon.SinonStub<any[], any>;
    let getBoolInputStub: sinon.SinonStub<any[], any>;
    let prependPathStub: sinon.SinonStub<any[], any>;
    let warningStub: sinon.SinonStub<any[], any>;
    let getVariableStub: sinon.SinonStub<any[], any>;
    let whichStub: sinon.SinonStub<any[], any>;

    before(() => {
        // stub task library
        getInputStub = ImportMock.mockFunction(tl, "getInput");
        getBoolInputStub = ImportMock.mockFunction(tl, "getBoolInput");
        prependPathStub = ImportMock.mockFunction(tl, "prependPath");
        warningStub = ImportMock.mockFunction(tl, "warning");

        // stub task library with default behaviors
        getVariableStub = ImportMock.mockFunction(tl, "getVariable");
        getVariableStub.withArgs("Agent.TempDirectory").returns(".");
        whichStub = ImportMock.mockFunction(tl, "which").returns("/bin/dotnet");
    });

    after(() => {
        // restore original behavior
        getInputStub.restore();
        getBoolInputStub.restore();
        whichStub.restore();
        getVariableStub.restore();
        prependPathStub.restore();
        warningStub.restore();
    });

    let toolStub: sinon.SinonStub<any[], any>;
    let toolMock: MockManager<tr.ToolRunner>;
    let mockTool: tr.ToolRunner;
    let argMock: sinon.SinonStub<any[], any>;
    let originalPath: string | undefined;
    beforeEach(() => {
        // create mock ToolRunner instance
        toolMock = ImportMock.mockClass(tr, "ToolRunner");
        mockTool = toolMock.getMockInstance();
        argMock = toolMock.mock("arg", mockTool);

        // mock tl.tool method
        toolStub = ImportMock.mockFunction(tl, "tool").returns(mockTool);

        // store original path
        originalPath = process.env.PATH;
    });

    afterEach(() => {
        // reset non-default task stubs
        getInputStub.reset();
        getBoolInputStub.reset();
        prependPathStub.reset();
        warningStub.reset();

        // restore tl.tool method
        toolStub.restore();

        // reset path
        process.env.PATH = originalPath;
    });

    it("uses previously installed dotnet-nlu extension", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);

        // run test
        const tool = await getNLUToolRunner();

        // assert calls
        const calls = argMock.getCalls();
        expect(tool).to.equal(mockTool);
        expect(calls.length).to.equal(3);

        // exec version check
        expect(calls[0].calledWith("nlu")).to.be.ok;
        expect(calls[1].calledWith("--version")).to.be.ok;

        // init final tool
        expect(calls[2].calledWith("nlu")).to.be.ok;

        // assert output
        expect(tool).to.equal(mockTool);
    });

    it("installs when dotnet-nlu not installed", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(1);
        execStub.onCall(1).returns(0);

        // run test
        await getNLUToolRunner();

        // assert calls
        const calls = argMock.getCalls();
        expect(calls.length).to.equal(8);

        // exec version check
        expect(calls[0].calledWith("nlu")).to.be.ok;
        expect(calls[1].calledWith("--version")).to.be.ok;

        // exec dotnet-nlu install
        expect(calls[2].calledWith("tool")).to.be.ok;
        expect(calls[3].calledWith("install")).to.be.ok;
        expect(calls[4].calledWith("dotnet-nlu")).to.be.ok;
        expect(calls[5].calledWith("--tool-path")).to.be.ok;
        expect(calls[6].calledWith(".dotnet")).to.be.ok;

        // assert path
        expect(prependPathStub.calledWith(".dotnet")).to.be.ok;
        expect(process.env.PATH!.startsWith(`.dotnet${path.delimiter}`)).to.be.ok;
    });

    it("installs using tool path input", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(1);
        execStub.onCall(1).returns(0);

        // stub inputs
        const toolPath = "foo";
        getInputStub.withArgs("toolPath").returns(toolPath);

        // run test
        await getNLUToolRunner();

        // assert path
        expect(prependPathStub.calledWith(toolPath)).to.be.ok;
        expect(process.env.PATH!.startsWith(`${toolPath}${path.delimiter}`)).to.be.ok;
    });

    it("installs dotnet-nlu for nupkgPath and toolVersion", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(1);
        execStub.onCall(1).returns(0);

        // stub inputs
        const nupkgPath = "foo";
        const toolVersion = "bar";
        getInputStub.withArgs("nupkgPath").returns(nupkgPath);
        getInputStub.withArgs("toolVersion").returns(toolVersion);

        // run test
        await getNLUToolRunner();

        // assert calls
        const calls = argMock.getCalls();
        expect(calls.length).to.equal(12);

        // exec dotnet-nlu install
        expect(calls[7].calledWith("--add-source")).to.be.ok;
        expect(calls[8].calledWith(nupkgPath)).to.be.ok;
        expect(calls[9].calledWith("--version")).to.be.ok;
        expect(calls[10].calledWith(toolVersion)).to.be.ok;
    });

    it("uninstalls dotnet-nlu for nupkgPath", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);
        execStub.onCall(1).returns(0);
        execStub.onCall(2).returns(0);

        // stub inputs
        const nupkgPath = "foo";
        getInputStub.withArgs("nupkgPath").returns(nupkgPath);

        // run test
        await getNLUToolRunner();

        // assert calls
        const calls = argMock.getCalls();
        expect(calls.length).to.equal(15);

        // exec dotnet-nlu uninstall
        expect(calls[2].calledWith("tool")).to.be.ok;
        expect(calls[3].calledWith("uninstall")).to.be.ok;
        expect(calls[4].calledWith("dotnet-nlu")).to.be.ok;
        expect(calls[5].calledWith("--tool-path")).to.be.ok;
        expect(calls[6].calledWith(".dotnet")).to.be.ok;

        // assert warning not called
        expect(warningStub.notCalled).to.be.ok;
    });

    it("uninstalls dotnet-nlu for toolVersion", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);
        execStub.onCall(1).returns(0);
        execStub.onCall(2).returns(0);

        // stub inputs
        const toolVersion = "foo";
        getInputStub.withArgs("toolVersion").returns(toolVersion);

        // run test
        await getNLUToolRunner();

        // assert calls
        const calls = argMock.getCalls();
        expect(calls.length).to.equal(15);

        // exec dotnet-nlu uninstall
        expect(calls[3].calledWith("uninstall")).to.be.ok;
    });

    it("uninstalls dotnet-nlu for toolPath", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);
        execStub.onCall(1).returns(0);
        execStub.onCall(2).returns(0);

        // stub inputs
        const toolPath = "foo";
        getInputStub.withArgs("toolPath").returns(toolPath);

        // run test
        await getNLUToolRunner();

        // assert calls
        const calls = argMock.getCalls();
        expect(calls.length).to.equal(13);

        // exec dotnet-nlu uninstall
        expect(calls[3].calledWith("uninstall")).to.be.ok;
    });

    it("warns if uninstall fails", async () => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);
        execStub.onCall(1).returns(1);
        execStub.onCall(2).returns(0);

        // stub inputs
        const toolVersion = "foo";
        getInputStub.withArgs("toolVersion").returns(toolVersion);

        await getNLUToolRunner();

        // assert warning
        expect(warningStub.calledOnce).to.be.ok;
    });

    it("throws if install fails", (done) => {
        // stub exec
        const execStub = toolMock.mock("exec");
        execStub.onCall(0).returns(0);
        execStub.onCall(1).returns(0);
        execStub.onCall(2).returns(1);

        // stub inputs
        const toolVersion = "foo";
        getInputStub.withArgs("toolVersion").returns(toolVersion);

        getNLUToolRunner()
            .then(() => done(new Error("Expected method to reject.")))
            .catch(() => done());
    });
});
