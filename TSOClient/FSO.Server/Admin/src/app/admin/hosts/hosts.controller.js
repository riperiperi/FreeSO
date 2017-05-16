'use strict';

angular.module('admin')
  .controller('HostsCtrl', function ($scope, Api, $mdDialog) {
      
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
          $scope.promise = Api.all("hosts").getList().then(function (hosts) {
             $scope.hosts = hosts;
          });
          return $scope.promise;
      }

      refresh();

  });
