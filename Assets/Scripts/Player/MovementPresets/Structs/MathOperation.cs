public struct MathOperation {
	public ushort code;
	public char operation;
	public float number;

	public float Apply(float num){
		if(operation == '+')
			return num + this.number;
		else if(operation == '*')
			return num * this.number;
		return number;
	}

	public bool Equals(MathOperation other){
		return this.code == other.code;
	}

	public override bool Equals(object obj){
		return obj is MathOperation other && Equals(other);
	}

	public override int GetHashCode(){
		return (int)code;
	}
}
