'use strict';

angular.module('admin')
  .controller('UsersDialogCtrl', function ($scope, Api, $mdDialog) {
      $scope.user = {};

      $scope.cancel = function () {
          $mdDialog.cancel();
      };
      $scope.ok = function () {
          console.log($scope.user);
          $mdDialog.hide($scope.user);
      };
  });
