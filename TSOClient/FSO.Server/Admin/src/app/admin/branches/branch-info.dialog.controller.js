'use strict';

angular.module('admin')
  .controller('BranchInfoDialogCtrl', function ($scope, Api, $mdDialog, branches, create) {
      $scope.create = create;
      $scope.req = (create) ? { last_version_number: 0, minor_version_number: 0 } : branches[0];
      $scope.addons = [];

      var refresh = function () {
          $scope.promise = Api.all("/updates/addons").getList().then(function (addons) {
             $scope.addons = addons;
          });
          return $scope.promise;
      }

      refresh();

      $scope.cancel = function () {
          $mdDialog.cancel();
      };
      $scope.ok = function () {
          console.log($scope.req);
          if ($scope.req.disableIncremental !== undefined) {
            if ($scope.req.disableIncremental) flags |= 1;
            else flags &= ~1;
            delete $scope.req.disableIncremental;
          }
          $mdDialog.hide($scope.req);
      };
  });
