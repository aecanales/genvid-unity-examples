// Copyright 2016-2020 Genvid Technologies Inc. All Rights Reserved.
let envs = require("envs");
import * as Consul from "consul";

let consul_addr = envs("CONSUL_HTTP_ADDR", "127.0.0.1:8500").split(":");
let port = consul_addr.pop();
let host = consul_addr.join(":");
export let consul = Consul({ host: host, port: port });

export let disco_url = envs("GENVID_DISCO_URL", "http://localhost:8080");
export let disco_secret = envs("GENVID_DISCO_SECRET", "discosecret");

export let webgateway_url = envs(
  "GENVID_WEBGATEWAY_URL",
  "http://localhost:8089"
);
export let webgateway_secret = envs(
  "GENVID_WEBGATEWAY_SECRET",
  "webgatewaysecret"
);

export interface ITaggedAddresses {
  lan: string;
  wan: string;
}

export interface IServiceEntry {
  Node: {
    ID: string;
    Node: string;
    Address: string;
    Datacenter: string;
    TaggedAddresses: ITaggedAddresses;
    Meta: {
      [name: string]: string;
    };
  };
  Service: {
    ID: string;
    Service: string;
    Tags: string[];
    Address: string;
    Meta: {
      [name: string]: string;
    };
    Port: number;
  };
  Checks: {
    Node: string;
    CheckID: string;
    Name: string;
    Status: string;
    Notes: string;
    Output: string;
    ServiceID: string;
    ServiceName: string;
    ServiceTags: string[];
  }[];
}

function wrapIPv6Address(address: string): string {
  if (address.includes(":")) {
    return `[${address}]`;
  }
  return address;
}

function watchService(serviceName: string, setUrl: (url: string) => void) {
  const watchOptions: Consul.Health.ServiceOptions = {
    service: serviceName,
    passing: true
  };

  const serviceWatch = consul.watch({
    method: consul.health.service,
    options: watchOptions
  });

  serviceWatch.on("change", (services: IServiceEntry[], _res) => {
    console.log(services);
    if (services.length === 0) {
      console.error(`${serviceName} service is not available from consul`);
    } else {
      let service = services[Math.floor(Math.random() * services.length)];
      let serviceUrl = `http://${wrapIPv6Address(service.Service.Address)}:${
        service.Service.Port
      }`;
      setUrl(serviceUrl);
      console.info(`Watch ${serviceName} url: ${serviceUrl}`);
    }
  });

  serviceWatch.on("error", err => {
    console.error(`${serviceName} watch error:`, err);
  });
}

watchService("disco", url => {
  disco_url = url;
});
watchService("webgateway", url => {
  webgateway_url = url;
});
