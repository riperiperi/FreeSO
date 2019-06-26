'use strict';

angular.module('admin')
  .controller('UpdatesCtrl', function ($scope, Api, $mdDialog) {
      
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

      setTimeout(function() {
          $('.pevents__initial').removeClass('pevents__initial');
      }, 1000);

      var refresh = function () {
          var offset = ($scope.query.page - 1) * $scope.query.limit;
          $scope.promise = Api.all("/updates").getList({ offset: offset, limit: $scope.query.limit, order: $scope.query.order}).then(function (updates) {
             $scope.updates = updates;
          });
          return $scope.promise;
      }

      refresh();


      $scope.showCreateUpdate = function (event) {
          $mdDialog.show({
              controller: 'UpdateCreateDialogCtrl',
              templateUrl: 'app/admin/updates/update-create.dialog.html',
              parent: angular.element(document.body),
              targetEvent: event,
              clickOutsideToClose: true,
              locals: {
                  updates: $scope.selected.map(function (x) { return x.update_id; })
              }
          })
            .then(function (answer) {
                Api.all("updates").post(answer).then(function (result) {
                    $scope.showUpdateProgress(result.taskID);
                    console.log(result);
                });
            }, function () {

            });
      }

      $scope.showUpdateProgress = function (taskID) {
          $mdDialog.show({
              controller: 'UpdateProgressDialogCtrl',
              templateUrl: 'app/admin/updates/update-progress.dialog.html',
              parent: angular.element(document.body),
              targetEvent: event,
              clickOutsideToClose: false,
              locals: {
                  updateTaskID: taskID
              }
          })
            .then(function (answer) {
                refresh();
            }, function () {

            });
      }

      $scope.showSchedule = function (event) {

      }

  });
