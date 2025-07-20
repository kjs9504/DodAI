package com.ohgiraffers.dodai.dto;

import java.util.List;

public class AcceptedTaskListDTO {
    private List<AcceptedTaskDTO> tasks;

    public AcceptedTaskListDTO() {}

    public AcceptedTaskListDTO(List<AcceptedTaskDTO> tasks) {
        this.tasks = tasks;
    }

    public List<AcceptedTaskDTO> getTasks() {
        return tasks;
    }

    public void setTasks(List<AcceptedTaskDTO> tasks) {
        this.tasks = tasks;
    }
} 