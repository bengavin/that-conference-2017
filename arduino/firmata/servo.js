var five = require('johnny-five');
var board = new five.Board({});

board.on('ready', function() {
	console.log('Ready!');
	
	var control = new five.Sensor({
		pin: "A0",
		freq: 100
	});
		
	var led = five.Led(11);	
	
	var servo = five.Servo({
		pin: 9,
		range: [0, 180],
		invert: true
	});

	servo.center();
	
	control.on('change', function() {
		var scaledVal = this.scaleTo(0, 180);
		var ledScaledVal = this.scaleTo(0, 255);
		
		console.log('Raw Value: ', this.raw, 'Scaled Value: ', scaledVal);
		
		servo.to(scaledVal);
		led.brightness(ledScaledVal);
	});
	
	this.on('exit', function() {
		led.off();
		servo.stop();
	});
});
