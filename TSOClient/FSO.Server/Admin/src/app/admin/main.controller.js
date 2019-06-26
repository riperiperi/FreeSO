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
          },
          {
              link: '/admin/updates',
              title: 'Updates',
              icon: 'system_update'
          },
          {
              link: '/admin/addons',
              title: 'Update Addons',
              icon: 'archive'
          },
          {
              link: '/admin/branches',
              title: 'Update Branches',
              icon: 'call_split'
          }
      ];

      $scope.navigateTo = function (item) {
          $location.path(item.link);
      }
  });
