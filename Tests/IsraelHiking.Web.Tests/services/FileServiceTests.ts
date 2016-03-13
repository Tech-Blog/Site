﻿/// <reference path="../../../isrealhiking.web/scripts/typings/ng-file-upload/ng-file-upload.d.ts" />
/// <reference path="../../../isrealhiking.web/common/constants.ts" />
/// <reference path="../../../isrealhiking.web/services/fileservice.ts" />

module IsraelHiking.Tests {
    describe("File Service", () => {
        var $http: angular.IHttpService;
        var $httpBackend: angular.IHttpBackendService;
        var Upload: angular.angularFileUpload.IUploadService;
        var FileSaver: Services.IFileSaver;
        var fileService: Services.FileService;

        beforeEach(() => {
            angular.mock.module("ngFileUpload");
            angular.mock.module("ngFileSaver");
            angular.mock.inject((_$http_: angular.IHttpService, _$httpBackend_: angular.IHttpBackendService, _Upload_: angular.angularFileUpload.IUploadService, _FileSaver_: Services.IFileSaver) => { // 
                // The injector unwraps the underscores (_) from around the parameter names when matching
                $http = _$http_;
                $httpBackend = _$httpBackend_;
                Upload = _Upload_;
                FileSaver = _FileSaver_;
                fileService = new Services.FileService($http, Upload, FileSaver);
            });
        });

        it("Should save to file", () => {
            spyOn(FileSaver, "saveAs");
            $httpBackend.whenPOST(Common.Urls.files + "?format=format").respond(btoa("bytes"));

            fileService.saveToFile("file.name", "format", {} as Common.DataContainer);

            $httpBackend.flush();
            expect(FileSaver.saveAs).toHaveBeenCalled();
        });
        
        it("Should open from file", () => {
            spyOn(Upload, "upload");

            fileService.openFromFile(new Blob([""]) as File);

            expect(Upload.upload).toHaveBeenCalled();
        });

        it("Should open from url", () => {
            $httpBackend.whenGET(Common.Urls.files + "?url=url").respond({ markers: [{} as Common.MarkerData] } as Common.DataContainer);

            fileService.openFromUrl("url")
                .success((data) => expect(data.markers.length).toBe(1));

            $httpBackend.flush();
        });
    });
}