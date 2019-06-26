'use strict';

angular.module('admin', ['ngSanitize', 'restangular', 'ui.router', 'ngMaterial', 'ngMdIcons', 'md.data.table', 'angularMoment'])
  .config(function ($stateProvider, $urlRouterProvider, $mdThemingProvider) {

      var restoreAuth = function (Auth) {
          return Auth.restore();
      };

      var resolve = ['Auth', restoreAuth];

      $stateProvider
        .state('login', {
            url: '/login',
            templateUrl: 'app/login/login.html',
            controller: 'LoginCtrl'
        }).state('admin', {
            url: '/admin',
            templateUrl: 'app/admin/main.html',
            controller: 'MainCtrl',
            requiresAuth: true,
            resolve: resolve
        }).state('admin.users', {
            url: '/users',
            controller: 'UsersCtrl',
            templateUrl: 'app/admin/users/users.html',
            requiresAuth: true,
            resolve: resolve
        }).state('admin.shards', {
            url: '/shards',
            controller: 'ShardsCtrl',
            templateUrl: 'app/admin/shards/shards.html',
            requiresAuth: true,
            resolve: resolve
        }).state('admin.hosts', {
            url: '/hosts',
            controller: 'HostsCtrl',
            templateUrl: 'app/admin/hosts/hosts.html',
            requiresAuth: true,
            resolve: resolve
        }).state('admin.tasks', {
            url: '/tasks',
            controller: 'TasksCtrl',
            templateUrl: 'app/admin/tasks/tasks.html',
            requiresAuth: true,
            resolve: resolve
        }).state('admin.updates', {
            url: '/updates',
            controller: 'UpdatesCtrl',
            templateUrl: 'app/admin/updates/updates.html',
            requiresAuth: true,
            resolve: resolve
        }).state('admin.addons', {
            url: '/addons',
            controller: 'AddonsCtrl',
            templateUrl: 'app/admin/addons/addons.html',
            requiresAuth: true,
            resolve: resolve
        }).state('admin.branches', {
            url: '/branches',
            controller: 'BranchesCtrl',
            templateUrl: 'app/admin/branches/branches.html',
            requiresAuth: true,
            resolve: resolve
        });

    $urlRouterProvider.otherwise('/login');


    var customBlueMap = $mdThemingProvider.extendPalette('light-blue', {
        'contrastDefaultColor': 'light',
        'contrastDarkColors': ['50'],
        '50': 'ffffff'
    });
    $mdThemingProvider.definePalette('customBlue', customBlueMap);
    $mdThemingProvider.theme('default')
      .primaryPalette('customBlue', {
          'default': '500',
          'hue-1': '50'
      })
      .accentPalette('pink');
    $mdThemingProvider.theme('input', 'default')
          .primaryPalette('grey')
  }).run(function ($rootScope, $location, Auth) {
    Auth.restore();

    $rootScope.$on('$stateChangeError',
        function (event, toState, toParams, fromState, fromParams) {
            $location.path('/login');
        });

});
