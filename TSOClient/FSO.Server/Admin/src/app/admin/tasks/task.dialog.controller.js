'use strict';

angular.module('admin')
  .controller('TaskDialogCtrl', function ($scope, $mdDialog, Api) {
      $scope.task = {parameter: {}};

      //Side data
      Api.all("/shards").getList().then(function (shards) {
          $scope.shards = shards;
      });

      $scope.cancel = function () {
          $mdDialog.cancel();
      };
      $scope.ok = function () {
          $mdDialog.hide($scope.task);
      };
  });
