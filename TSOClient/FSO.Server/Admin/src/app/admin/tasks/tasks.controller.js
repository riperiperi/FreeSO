'use strict';

angular.module('admin')
  .controller('TasksCtrl', function ($scope, Api, $mdDialog) {
      
      $scope.query = {
          filter: '',
          limit: 10,
          page: 1
      };

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
          var offset = ($scope.query.page - 1) * $scope.query.limit;
          return Api.all("/tasks").getList({offset: offset, limit: $scope.query.limit}).then(function (tasks) {
              $scope.tasks = tasks;
          });
      }

      refresh();



      $scope.showAdd = function (event) {
          $mdDialog.show({
              controller: 'TaskDialogCtrl',
              templateUrl: 'app/admin/tasks/task.dialog.html',
              parent: angular.element(document.body),
              targetEvent: event,
              clickOutsideToClose: true
          })
            .then(function (answer) {
                Api.all("/tasks/request").post(answer).then(function (newTask) {
                    refresh();
                });
            }, function () {

            });
      }


  });
