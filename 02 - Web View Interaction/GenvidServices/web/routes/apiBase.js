"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
// Copyright 2016-2020 Genvid Technologies Inc. All Rights Reserved.
const express = require("express");
const streams = require("./streams.t");
const commands = require("./commands");
const basicAuth = require("../utils/auth");
let router = express.Router();
router.use("/public", streams.router);
router.use("/admin", basicAuth.authenticate, commands.router);
module.exports = router;
//# sourceMappingURL=apiBase.js.map