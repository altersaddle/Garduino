
int firstValue = 0;    			
int secondValue = 1000;  			

void setup() {
  Serial.begin(9600); 				//Set baud rate to 9600 on the Arduino
}

void loop() {
  Serial.print("SINGLE\t1\t");
  Serial.println(firstValue); 		 //print the value to Serial monitor
  
  Serial.print("DOUBLE\t2\t");
  Serial.print(firstValue);
  Serial.print("\t");
  Serial.println(secondValue);
  
  firstValue += 50;
  secondValue -= 50;
  
  if (firstValue > 1000) {
    firstValue = 0;
  }
  if (secondValue < 0) {
    secondValue = 1000;
  }
  delay(5000);                        //delay for 25 seconds
}


