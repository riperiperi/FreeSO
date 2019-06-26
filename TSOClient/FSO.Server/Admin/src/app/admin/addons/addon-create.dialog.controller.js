'use strict';

angular.module('admin')
  .controller('AddonCreateDialogCtrl', function ($scope, Api, $mdDialog, addons) {
      $scope.req = { };
      $scope.clientFilename = "No file chosen.";
      $scope.serverFilename = "No file chosen.";
      $scope.clientFile = null;
      $scope.serverFile = null;

      $scope.clientChanged = function (evt) {
          $scope.clientFile = evt.target.files[0];
          $scope.clientFilename = $scope.clientFile.name;
      }

      $scope.serverChanged = function (evt) {
          $scope.serverFile = evt.target.files[0];
          $scope.serverFilename = $scope.serverFile.name;
      }

      $scope.cancel = function () {
          $mdDialog.cancel();
      };
      $scope.ok = function () {
          var form = new FormData();
          form.append("name", $scope.req.name);
          if ($scope.req.description) form.append("description", $scope.req.description);
          form.append("clientAddon", $scope.clientFile);
          if ($scope.serverFile) form.append("serverAddon", $scope.serverFile);
          $mdDialog.hide(form);
      };
  });
