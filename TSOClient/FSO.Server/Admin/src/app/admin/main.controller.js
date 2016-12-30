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
              icon: 'terrain'
          },
          {
              link: '/admin/hosts',
              title: 'Hosts',
              icon: 'cloud'
          },
          {
              link: '/admin/tasks',
              title: 'Tasks',
              icon: 'alarm'
          }
      ];

      $scope.navigateTo = function (item) {
          $location.path(item.link);
      }
  });
