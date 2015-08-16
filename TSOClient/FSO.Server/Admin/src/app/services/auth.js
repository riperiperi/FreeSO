'use strict';

angular.module('admin').service('Auth', function ($window, Api, Token, $rootScope, $q, $location) {

    var self = this;

    this.loggedIn = false;

    function bootstrapUser(token, expires_in, deferred) {
        Token.setToken(token);
        Api.one('users', 'current').get().then(function (val) {
            $rootScope.currentUser = val;
            self.loggedIn = true;
            deferred.resolve();
        }, function (err) {
            deferred.reject();
        });
    }

    var restorePromise = null;

    this.restore = function () {
        if (restorePromise != null) {
            return restorePromise;
        }

        var deferred = $q.defer();
        if (Token.getTokenImmediately() == null) {
            var authToken = $window.sessionStorage.getItem('authToken');
            if (authToken != null) {
                authToken = JSON.parse(authToken);
                if (authToken.expires > new Date().getTime()) {
                    /** Still active **/
                    Api.setBaseUrl(authToken.api);
                    bootstrapUser(authToken.access_token, (new Date().getTime() - authToken.expires) / 1000, deferred);
                    return;
                }
            }

            deferred.reject();
        } else {
            deferred.resolve();
        }

        restorePromise = deferred.promise;
        return restorePromise;
    }

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
                
                $window.sessionStorage.setItem('authToken', JSON.stringify({
                    api: apiUrl,
                    access_token: result.access_token,
                    expires: new Date().getTime() + (result.expires_in * 1000)
                }));
                
        restorePromise = null;
                bootstrapUser(result.access_token, result.expires_in, deferred);
            } else {
                deferred.reject();
            }
        }, function (err) {
            deferred.reject(err);
        });

        return deferred.promise;
    }

});
