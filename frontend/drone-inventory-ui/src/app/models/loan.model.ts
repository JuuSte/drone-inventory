export interface Loan {
  id: number;
  studentGroupName: string;
  droneName: string;
  batteries: number;
  propellers: number;
  loanDate: string;
}

export interface LoanHistoryEntry extends Loan {
  returnDate: string;
}

export interface CreateLoanRequest {
  droneId: number;
  studentGroupId: number;
  batteries: number;
  propellers: number;
}
