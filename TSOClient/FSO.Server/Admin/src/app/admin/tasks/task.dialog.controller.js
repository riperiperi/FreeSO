'use strict';

angular.module('admin')
  .controller('TaskDialogCtrl', function ($scope, $mdDialog) {
      $scope.task = {};

      $scope.cancel = function () {
          $mdDialog.cancel();
      };
      $scope.ok = function () {
          $mdDialog.hide($scope.task);
      };
  });
