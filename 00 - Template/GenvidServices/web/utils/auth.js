"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.authenticate = void 0;
const basicAuth = require("basic-auth");
let admins = {
    "admin": { password: "admin" },
    "user1": { password: "user1" },
    "user2": { password: "user2" },
};
function authenticate(req, res, next) {
    let user = basicAuth(req);
    if (!user || !admins[user.name] || admins[user.name].password !== user.pass) {
        res.set("WWW-Authenticate", "Basic realm='example'");
        return res.status(401).send();
    }
    return next();
}
exports.authenticate = authenticate;
;
//# sourceMappingURL=auth.js.map