const crypto = require('crypto');

function AddHash(r){
    let h = r.uri;
    var hash = crypto.createHash('md5').update(h).digest('hex');
    r.headersOut["x-Cache-Key"] = hash;
}

export default{AddHash}

AddHash("/test/page");