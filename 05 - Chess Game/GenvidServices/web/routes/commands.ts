// Copyright 2016-2020 Genvid Technologies Inc. All Rights Reserved.
import * as express from "express";
import * as request from "request";
import * as rp from "request-promise";

import * as config from "../utils/config";

export let router = express.Router();

export interface ICommandRequest {
  id: string;
  value: string;
}

router.get("/test", async (_req, res) => {
  res.status(200).send("OK");
});

router.post("/commands/game", async (req, res) => {
  let body: ICommandRequest = req.body;

  let commandRequest: request.UriOptions & rp.RequestPromiseOptions = {
    method: "POST",
    body: body,
    baseUrl: config.webgateway_url,
    uri: "/commands/game",
    headers: {
      secret: config.webgateway_secret
    },
    json: true
  };

  try {
    await rp(commandRequest);
    res.status(200).send();
  } catch (err) {
    res.status(500).send(err.error);
  }
});
