(function () {
    if (tag.title) {
        var parts = [];
        if (parseInt(tag.disccount) != 1 && parseInt(tag.disc)) {
            parts.push(tag.disc);
        }
        if (tag.track) {
            parts.push(zeropad2(tag.track, tag.trackcount, 2));
        }
        parts.push(tag.title);
        return parts.join(" - ");
    } return filename(file);
})()