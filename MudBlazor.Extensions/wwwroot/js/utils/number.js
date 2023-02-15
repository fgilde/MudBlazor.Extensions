class MudExNumber {
    static constrain (number, min, max) {
        var x = parseFloat(number);
        if (min === null) {
            min = number;
        }

        if (max === null) {
            max = number;
        }
        return (x < min) ? min : ((x > max) ? max : x);
    }
}

window.MudExNumber = MudExNumber;