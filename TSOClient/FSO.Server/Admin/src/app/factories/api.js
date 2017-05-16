'use strict';

angular.module('admin').factory('Api', function (Restangular, Token, $rootScope) {
    return Restangular.withConfig(function (RestangularConfigurer) {

        RestangularConfigurer.addFullRequestInterceptor(function (element, operation, what, url, headers, params) {
            var token = Token.getTokenImmediately();
            if (token != null) {
                headers['Authorization'] = "bearer " + token;
            }

            return {
                headers: headers,
                params: params,
                element: element
            };
        });

        RestangularConfigurer.addResponseInterceptor(function (data, operation, what, url, response, deferred) {
            if (data.error) {
                deferred.reject(data.error);
                return;
            }

            if (operation === "getList") {
                data.total = response.headers('X-Total-Count');
                if (!data.total) {
                    data.total = data.length;
                }
            }

            return data;
        });


    });
});
