#include <dht.h>

dht DHT;


#define DHT22_PIN 2

int lightSensorPin = 0;    				// select the input pin for the photocell
int lightSensorValue = 0;  				// variable to store the value coming from the photocell

int soilSensorPin = 1;
int soilSensorValue = 0;

void setup() {
  Serial.begin(9600); 				//Set baud rate to 9600 on the Arduino
}

void loop() {
                                          // read the value from the sensor:
  lightSensorValue = analogRead(lightSensorPin);  //get the voltage value from input pin
  Serial.print("LIGHT\t1\t");
  Serial.println(lightSensorValue); 		 //print the value to Serial monitor
  
  soilSensorValue = analogRead(soilSensorPin);
  Serial.print("SOIL\t1\t");
  Serial.println(soilSensorValue);
  
    // READ DATA
  Serial.print("DHT22\t2\t");
  int chk = DHT.read22(DHT22_PIN);
  switch (chk)
  {
    case DHTLIB_OK:  
                // DISPLAY DATA
                Serial.print(DHT.humidity, 1);
                Serial.print("\t");
                Serial.println(DHT.temperature, 1);
                break;
    case DHTLIB_ERROR_CHECKSUM: 
                Serial.println("Checksum error,\t-1"); 
                break;
    case DHTLIB_ERROR_TIMEOUT: 
                Serial.println("Time out error,\t-1"); 
                break;
    default: 
                Serial.println("Unknown error,\t-1"); 
                break;
  }

  
  delay(5000);                        //delay for 25 seconds
}

