'use strict';

angular.module('admin')
  .controller('UpdateCreateDialogCtrl', function ($scope, Api, $mdDialog, updates) {
      $scope.req = { };
      $scope.branches = [];

      var refresh = function () {
          $scope.promise = Api.all("/updates/branches").getList().then(function (branches) {
             $scope.branches = branches;
          });
          return $scope.promise;
      }

      refresh();

      $scope.catalogChanged = function (evt) {
          var file = evt.target.files[0];

          var reader = new FileReader();
          reader.onload = function(){
              var text = reader.result;
              $scope.req.catalog = text;
          };
          reader.readAsText(file);
      }


      $scope.cancel = function () {
          $mdDialog.cancel();
      };
      $scope.ok = function () {
          if ($scope.req.date) {
              $scope.req.scheduledEpoch = ($scope.req.date.getTime() / 1000) | 0;
              delete $scope.req.date;
          }
          console.log($scope.req);
          $mdDialog.hide($scope.req);
      };
  });
