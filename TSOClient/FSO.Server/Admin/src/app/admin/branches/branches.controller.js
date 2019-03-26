'use strict';

angular.module('admin')
  .controller('BranchesCtrl', function ($scope, Api, $mdDialog) {
      
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
          $scope.promise = Api.all("updates/branches").getList().then(function (branches) {
             $scope.branches = branches;
          });
          return $scope.promise;
      }

      refresh();



      $scope.showCreateBranch = function (event) {
          $mdDialog.show({
              controller: 'BranchInfoDialogCtrl',
              templateUrl: 'app/admin/branches/branch-info.dialog.html',
              parent: angular.element(document.body),
              targetEvent: event,
              clickOutsideToClose: true,
              locals: {
                  branches: $scope.selected,
                  create: true
              }
          })
            .then(function (branchInfo) {
                Api.all('updates/branches').post(branchInfo).then(function (result) {
                    console.log(result);
                    refresh();
                });
            }, function () {

            });
      }

      $scope.showUpdateBranch = function (event) {
          $mdDialog.show({
              controller: 'BranchInfoDialogCtrl',
              templateUrl: 'app/admin/branches/branch-info.dialog.html',
              parent: angular.element(document.body),
              targetEvent: event,
              clickOutsideToClose: true,
              locals: {
                  branches: $scope.selected,
                  create: false
              }
          })
            .then(function (branchInfo) {
                Api.one('updates/branches', branchInfo.branch_id).customPOST(branchInfo).then(function (result) {
                    console.log(result);
                    refresh();
                });
            }, function () {

            });
      }

  });
