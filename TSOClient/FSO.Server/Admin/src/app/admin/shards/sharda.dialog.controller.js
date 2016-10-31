'use strict';

angular.module('admin')
  .controller('ShardAnnDialogCtrl', function ($scope, Api, $mdDialog) {
      $scope.ann = {};

      $scope.cancel = function () {
          $mdDialog.cancel();
      };
      $scope.ok = function () {
          console.log($scope.ann);
          $mdDialog.hide($scope.ann);
      };
  });
