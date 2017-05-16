'use strict';

angular.module('admin')
  .controller('ShardAnnDialogCtrl', function ($scope, Api, $mdDialog, shards) {
      $scope.ann = {shard_ids: shards};

      $scope.cancel = function () {
          $mdDialog.cancel();
      };
      $scope.ok = function () {
          console.log($scope.ann);
          $mdDialog.hide($scope.ann);
      };
  });
