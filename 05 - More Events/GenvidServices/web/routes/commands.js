"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.router = void 0;
// Copyright 2016-2020 Genvid Technologies Inc. All Rights Reserved.
const express = require("express");
const rp = require("request-promise");
const config = require("../utils/config");
exports.router = express.Router();
exports.router.get("/test", (_req, res) => __awaiter(void 0, void 0, void 0, function* () {
    res.status(200).send("OK");
}));
exports.router.post("/commands/game", (req, res) => __awaiter(void 0, void 0, void 0, function* () {
    let body = req.body;
    let commandRequest = {
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
        yield rp(commandRequest);
        res.status(200).send();
    }
    catch (err) {
        res.status(500).send(err.error);
    }
}));
//# sourceMappingURL=commands.js.map