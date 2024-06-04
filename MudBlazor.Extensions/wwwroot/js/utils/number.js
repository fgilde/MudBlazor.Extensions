class MudExNumber {
    static constrain(number, min, max) {
        var x = parseFloat(number);
        if (min === null) {
            min = number;
        }

        if (max === null) {
            max = number;
        }
        return (x < min) ? min : ((x > max) ? max : x);
    }

    static async md5(email) {
        return Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(email.trim().toLowerCase()))))
            .map(b => b.toString(16).padStart(2, '0'))
            .join('');
    }

}

window.MudExNumber = MudExNumber;