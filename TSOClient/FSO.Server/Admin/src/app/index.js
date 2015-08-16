'use strict';

angular.module('admin', ['ngSanitize', 'restangular', 'ui.router', 'ngMaterial', 'ngMdIcons'])
  .config(function ($stateProvider, $urlRouterProvider, $mdThemingProvider){
      $stateProvider
        .state('login', {
            url: '/login',
            templateUrl: 'app/login/login.html',
            controller: 'LoginCtrl'
        }).state('admin', {
            url: '/admin',
            templateUrl: 'app/admin/main.html',
            controller: 'MainCtrl'
        }).state('admin.users', {
            url: '/users',
            controller: 'UsersCtrl',
            templateUrl: 'app/admin/users/users.html'
        });;

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
  })
;
