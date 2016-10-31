'use strict';

angular.module('admin')
  .controller('ShardsCtrl', function ($scope, Api, $mdDialog) {
      
      $scope.query = {
          filter: '',
          order: '',
          limit: 10,
          page: 1
      };

      $scope.toolbar = {
          open: false,
          count: 0
      }

      $scope.search = function (predicate) {
          $scope.filter = predicate;
          return refresh();
      };

      $scope.onOrderChange = function (order) {
          return refresh();
      };

      $scope.onPaginationChange = function (page, limit) {
          return refresh();
      };

      var refresh = function () {
          //var offset = ($scope.query.page - 1) * $scope.query.limit;
          //return Api.all("/shards").getList({offset: offset, limit: $scope.query.limit}).then(function (shards) {
          //   $scope.shards = shards;
          //});
      }

      refresh();



      $scope.showAnnouncement = function (event) {
          $mdDialog.show({
              controller: 'ShardAnnDialogCtrl',
              templateUrl: 'app/admin/shards/sharda.dialog.html',
              parent: angular.element(document.body),
              targetEvent: event,
              clickOutsideToClose: true
          })
            .then(function (answer) {
                Api.all("shards/announce").post(answer).then(function (result) {
                    console.log(result);
                    refresh();
                });
            }, function () {

            });
      }

      $scope.showShutdown = function (event) {
          $mdDialog.show({
              controller: 'ShardXDialogCtrl',
              templateUrl: 'app/admin/shards/shardx.dialog.html',
              parent: angular.element(document.body),
              targetEvent: event,
              clickOutsideToClose: true
          })
            .then(function (answer) {
                Api.all("shards/shutdown").post(answer).then(function (result) {
                    console.log(result);
                    refresh();
                });
            }, function () {

            });
      }


  });
