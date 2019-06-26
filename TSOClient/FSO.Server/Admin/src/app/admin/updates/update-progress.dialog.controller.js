'use strict';

angular.module('admin')
  .controller('UpdateProgressDialogCtrl', function ($scope, Api, $mdDialog, updateTaskID) {
      $scope.progressPct = 0;
      $scope.progressString = "Waiting for server to begin...";
      $scope.progressError = null;

      var status = {
        FAILURE: 0,

        PREPARING: 1,
        DOWNLOADING_CLIENT: 2,
        DOWNLOADING_SERVER: 3,
        DOWNLOADING_CLIENT_ADDON: 4,
        DOWNLOADING_SERVER_ADDON: 5,
        EXTRACTING_CLIENT: 6,
        EXTRACTING_CLIENT_ADDON: 7,
        BUILDING_DIFF: 8,
        BUILDING_INCREMENTAL_UPDATE: 9,
        BUILDING_CLIENT: 10,
        PUBLISHING_CLIENT: 11,

        EXTRACTING_SERVER: 12,
        EXTRACTING_SERVER_ADDON: 13,
        BUILDING_SERVER: 14,
        PUBLISHING_SERVER: 15,

        SCHEDULING_UPDATE: 16,
        SUCCESS: 17
      }

      var statusString = {};
      statusString[status.FAILURE] = "Update generation failed!",

      statusString[status.PREPARING] = "Preparing to create update...",
      statusString[status.DOWNLOADING_CLIENT] = "Downloading client files from source...",
      statusString[status.DOWNLOADING_SERVER] = "Downloading server files from source...",
      statusString[status.DOWNLOADING_CLIENT_ADDON] = "Downloading client addon from source...",
      statusString[status.DOWNLOADING_SERVER_ADDON] = "Downloading server addon from source...",
      statusString[status.EXTRACTING_CLIENT] = "Extracting client files...",
      statusString[status.EXTRACTING_CLIENT_ADDON] = "Extracting addon files on top of client...",
      statusString[status.BUILDING_DIFF] = "Building delta between this update and the last...",
      statusString[status.BUILDING_INCREMENTAL_UPDATE] = "Building incremental zip...",
      statusString[status.BUILDING_CLIENT] = "Building client zip...",
      statusString[status.PUBLISHING_CLIENT] = "Publishing client zip...",

      statusString[status.EXTRACTING_SERVER] = "Extracting server files...",
      statusString[status.EXTRACTING_SERVER_ADDON] = "Extracting addon files on top of server...",
      statusString[status.BUILDING_SERVER] = "Building server zip...",
      statusString[status.PUBLISHING_SERVER] = "Publishing server zip...",

      statusString[status.SCHEDULING_UPDATE] = "Scheduling update...",
      statusString[status.SUCCESS] = "Done!"

      var statusLoop = function () {
          $scope.promise = Api.one("/updates/updateTask", updateTaskID).get().then(function (info) {
              if (info.code == 0) {
                  window.clearInterval(loop);
                  $scope.progressError = info.failure;
              } else if (info.code == status.SUCCESS) {
                  done();
              } else {
                  $scope.progressString = statusString[info.code];
                  $scope.progressPct = info.estimatedProgress*100;
              }
          });
          return $scope.promise;
      }

      var loop = window.setInterval(statusLoop, 500);

      $scope.close = function () {
          window.clearInterval(loop);
          $mdDialog.cancel();
      };

      var done = function() {
           window.clearInterval(loop);
          $mdDialog.hide(true);
      }
  });
