'use strict';

angular.module('admin')
  .controller('MainCtrl', function ($scope, $mdSidenav, $location) {
      $scope.toggleSidenav = function (menuId) {
          $mdSidenav(menuId).toggle();
      };
      $scope.menu = [
          {
              link: '/admin/users',
              title: 'Users',
              icon: 'group'
          },
          {
              link: '/admin/shards',
              title: 'Shards',
              icon: 'storage'
          }
      ];

      $scope.navigateTo = function (item) {
          $location.path(item.link);
      }
  });
