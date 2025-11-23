from typing import Optional, List
from fastapi import FastAPI, Depends, HTTPException
from sqlmodel import Field, Session, SQLModel, create_engine, select
from pydantic import BaseModel

# Database connection
DATABASE_URL = "mysql+pymysql://user:password@db/mech_ai_db"
engine = create_engine(DATABASE_URL)

# ============================================
# SQLModel Table Models (Database Schema)
# ============================================

class Make(SQLModel, table=True):
    __tablename__ = "Make"

    MakeID: Optional[int] = Field(default=None, primary_key=True)
    Name: str


class Car(SQLModel, table=True):
    __tablename__ = "Car"

    ModelID: Optional[int] = Field(default=None, primary_key=True)
    MakeID: int = Field(foreign_key="Make.MakeID")
    ModelName: str
    Year: int


class Procedure(SQLModel, table=True):
    __tablename__ = "Procedures"

    ProcedureID: Optional[int] = Field(default=None, primary_key=True)
    Name: str
    Description: Optional[str] = None


class Step(SQLModel, table=True):
    __tablename__ = "Steps"

    StepID: Optional[int] = Field(default=None, primary_key=True)
    Title: str
    Body: str
    YOLOClass: Optional[str] = None


class ProcedureStep(SQLModel, table=True):
    __tablename__ = "Procedure_Steps"

    ProcedureID: int = Field(foreign_key="Procedures.ProcedureID", primary_key=True)
    StepID: int = Field(foreign_key="Steps.StepID", primary_key=True)
    OrderNum: int


class CarProcedure(SQLModel, table=True):
    __tablename__ = "Car_Procedures"

    ModelID: int = Field(foreign_key="Car.ModelID", primary_key=True)
    ProcedureID: int = Field(foreign_key="Procedures.ProcedureID", primary_key=True)


# ============================================
# Pydantic Response Models (API Responses)
# ============================================

class StepInProcedureResponse(BaseModel):
    id: str
    instruction_id: str
    order: int


class ProcedureListResponse(BaseModel):
    id: str
    title: str
    description: str


class ProcedureDetailResponse(BaseModel):
    id: str
    title: str
    description: str
    steps: List[StepInProcedureResponse]


class StepDetailResponse(BaseModel):
    id: str
    title: str
    body: str
    yolo_class: Optional[str]


# ============================================
# FastAPI App
# ============================================

app = FastAPI(title="MechAI Backend API", version="1.0.0")


def get_session():
    with Session(engine) as session:
        yield session


# ============================================
# API Endpoints
# ============================================

@app.get("/api/health")
def health_check():
    """Check that the backend is alive."""
    return {"status": "ok", "message": "Backend is running"}


@app.get("/api/procedures", response_model=List[ProcedureListResponse])
def list_procedures(session: Session = Depends(get_session)):
    """List all procedures / jobs."""
    statement = select(Procedure)
    procedures = session.exec(statement).all()

    return [
        ProcedureListResponse(
            id=f"proc_{proc.ProcedureID}",
            title=proc.Name,
            description=proc.Description or ""
        )
        for proc in procedures
    ]


@app.get("/api/procedures/{procedure_id}", response_model=ProcedureDetailResponse)
def get_procedure(procedure_id: str, session: Session = Depends(get_session)):
    """Get a single procedure (title, short description, all steps)."""
    # Extract numeric ID from string format (e.g., "proc_1" -> 1)
    try:
        if procedure_id.startswith("proc_"):
            numeric_id = int(procedure_id.replace("proc_", ""))
        else:
            numeric_id = int(procedure_id)
    except ValueError:
        raise HTTPException(status_code=404, detail="Procedure not found")

    # Get procedure
    statement = select(Procedure).where(Procedure.ProcedureID == numeric_id)
    procedure = session.exec(statement).first()

    if not procedure:
        raise HTTPException(status_code=404, detail="Procedure not found")

    # Get steps for this procedure
    statement = (
        select(ProcedureStep)
        .where(ProcedureStep.ProcedureID == numeric_id)
        .order_by(ProcedureStep.OrderNum)
    )
    procedure_steps = session.exec(statement).all()

    steps = [
        StepInProcedureResponse(
            id=f"step_{ps.StepID}",
            instruction_id=f"instr_{ps.StepID}",
            order=ps.OrderNum
        )
        for ps in procedure_steps
    ]

    return ProcedureDetailResponse(
        id=f"proc_{procedure.ProcedureID}",
        title=procedure.Name,
        description=procedure.Description or "",
        steps=steps
    )


@app.get("/api/steps/{step_id}", response_model=StepDetailResponse)
def get_step(step_id: str, session: Session = Depends(get_session)):
    """Get one step with its instruction details."""
    # Extract numeric ID from string format (e.g., "step_3" -> 3)
    try:
        if step_id.startswith("step_"):
            numeric_id = int(step_id.replace("step_", ""))
        else:
            numeric_id = int(step_id)
    except ValueError:
        raise HTTPException(status_code=404, detail="Step not found")

    # Get step
    statement = select(Step).where(Step.StepID == numeric_id)
    step = session.exec(statement).first()

    if not step:
        raise HTTPException(status_code=404, detail="Step not found")

    return StepDetailResponse(
        id=f"step_{step.StepID}",
        title=step.Title,
        body=step.Body,
        yolo_class=step.YOLOClass
    )
