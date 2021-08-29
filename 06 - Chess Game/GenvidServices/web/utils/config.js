"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.webgateway_secret = exports.webgateway_url = exports.disco_secret = exports.disco_url = exports.consul = void 0;
// Copyright 2016-2020 Genvid Technologies Inc. All Rights Reserved.
let envs = require("envs");
const Consul = require("consul");
let consul_addr = envs("CONSUL_HTTP_ADDR", "127.0.0.1:8500").split(":");
let port = consul_addr.pop();
let host = consul_addr.join(":");
exports.consul = Consul({ host: host, port: port });
exports.disco_url = envs("GENVID_DISCO_URL", "http://localhost:8080");
exports.disco_secret = envs("GENVID_DISCO_SECRET", "discosecret");
exports.webgateway_url = envs("GENVID_WEBGATEWAY_URL", "http://localhost:8089");
exports.webgateway_secret = envs("GENVID_WEBGATEWAY_SECRET", "webgatewaysecret");
function wrapIPv6Address(address) {
    if (address.includes(":")) {
        return `[${address}]`;
    }
    return address;
}
function watchService(serviceName, setUrl) {
    const watchOptions = {
        service: serviceName,
        passing: true
    };
    const serviceWatch = exports.consul.watch({
        method: exports.consul.health.service,
        options: watchOptions
    });
    serviceWatch.on("change", (services, _res) => {
        console.log(services);
        if (services.length === 0) {
            console.error(`${serviceName} service is not available from consul`);
        }
        else {
            let service = services[Math.floor(Math.random() * services.length)];
            let serviceUrl = `http://${wrapIPv6Address(service.Service.Address)}:${service.Service.Port}`;
            setUrl(serviceUrl);
            console.info(`Watch ${serviceName} url: ${serviceUrl}`);
        }
    });
    serviceWatch.on("error", err => {
        console.error(`${serviceName} watch error:`, err);
    });
}
watchService("disco", url => {
    exports.disco_url = url;
});
watchService("webgateway", url => {
    exports.webgateway_url = url;
});
//# sourceMappingURL=config.js.map