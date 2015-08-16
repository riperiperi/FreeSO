'use strict';

angular.module('admin').service('Auth', function ($window, Api, Token, $rootScope, $q) {

    this.login = function (apiUrl, username, password) {
        apiUrl += "/admin";
        apiUrl = apiUrl.split("//admin").join("/admin");

        Api.setBaseUrl(apiUrl);

        var date_encoded = "grant_type=password&username=" + escape(username) + "&password=" + escape(password);

        var deferred = $q.defer();

        Api.all("oauth/token").customPOST(
            date_encoded,
            undefined, // put your path here
            undefined, // params here, e.g. {format: "json"}
            { 'Content-Type': "application/x-www-form-urlencoded; charset=UTF-8" }
        ).then(function (result) {
            if (result.access_token) {
                Token.setToken(result.access_token);

                Api.one('users', 'current').get().then(function (val) {
                    console.log(val);
                });

                deferred.resolve();
            } else {
                deferred.reject();
            }
        }, function (err) {
            deferred.reject(err);
        });

        return deferred.promise;
    }

});
