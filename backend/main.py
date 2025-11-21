from typing import Optional
from fastapi import FastAPI, Depends
from sqlmodel import Field, Session, SQLModel, create_engine, select

DATABASE_URL = "mysql+pymysql://user:password@db/mech_ai_db"
engine = create_engine(DATABASE_URL)

class Instruction(SQLModel, table=True):
    __tablename__ = "instructions"

    ID: Optional[int] = Field(default=None, primary_key=True)
    StepNum: int
    InstructionText: str

app = FastAPI()

def get_session():
    with Session(engine) as session:
        yield session

@app.get("/")
def read_instructions(session: Session = Depends(get_session)):
    statement = select(Instruction)
    results = session.exec(statement).all()
    return results