'use strict';

angular.module('admin')
  .controller('AddonsCtrl', function ($scope, Api, $mdDialog) {
      
      $scope.selected = [];

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
          $scope.promise = Api.all("updates/addons").getList().then(function (addons) {
             $scope.addons = addons;
          });
          return $scope.promise;
      }

      refresh();



      $scope.showCreateAddon = function (event) {
          $mdDialog.show({
              controller: 'AddonCreateDialogCtrl',
              templateUrl: 'app/admin/addons/addon-create.dialog.html',
              parent: angular.element(document.body),
              targetEvent: event,
              clickOutsideToClose: true,
              locals: {
                  addons: $scope.selected.map(function (x) { return x.addon_id; })
              }
          })
            .then(function (formData) {
                console.log(formData);
                Api.one('updates/uploadaddon').withHttpConfig({transformRequest: angular.identity})
                    .customPOST(formData, '', undefined, {'Content-Type': undefined})
                    .then(function (result) {
                        refresh();
                        console.log(result);
                });
            }, function () {

            });
      }

  });
