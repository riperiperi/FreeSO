'use strict';

angular.module('admin')
  .controller('UsersCtrl', function ($scope, Api, $mdDialog) {
      
      $scope.query = {
          filter: '',
          order: 'register_date',
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

      $scope.getRole = function (user) {
          if (user.is_admin) {
              return "Admin";
          } else if (user.is_moderator) {
              return "Moderator";
          } else {
              return "User";
          }
      }

      var refresh = function () {
          var offset = ($scope.query.page - 1) * $scope.query.limit;
          $scope.promise = Api.all("/users").getList({ offset: offset, limit: $scope.query.limit, order: $scope.query.order}).then(function (users) {
              $scope.users = users;
          });
          return $scope.promise;
      }

      refresh();



      $scope.showAdd = function (event) {
          $mdDialog.show({
              controller: 'UsersDialogCtrl',
              templateUrl: 'app/admin/users/users.dialog.html',
              parent: angular.element(document.body),
              targetEvent: event,
              clickOutsideToClose: true
          })
            .then(function (answer) {
                Api.all("/users").post(answer).then(function (newUser) {
                    console.log(newUser);
                    refresh();
                });
            }, function () {

            });
      }


  });
