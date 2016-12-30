'use strict';

angular.module('admin')
  .controller('ShardXDialogCtrl', function ($scope, Api, $mdDialog, shards) {
      $scope.sd = { timeout: 60, restart: true, shard_ids: shards };

      $scope.cancel = function () {
          $mdDialog.cancel();
      };
      $scope.ok = function () {
          console.log($scope.sd);
          $mdDialog.hide($scope.sd);
      };
  });
