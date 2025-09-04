import crypto from 'crypto';

function AddHash(r){
    let h = r.uri
    var hash = crypto.createHash('md5').update(h).digest('hex');
}

export default{AddHash}