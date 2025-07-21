package com.ohgiraffers.dodai.model;

import jakarta.persistence.*;
import org.hibernate.annotations.JdbcTypeCode;

import java.math.BigDecimal;
import java.time.Instant;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;

import static org.hibernate.type.SqlTypes.JSON;

@Entity
@Table(name = "fruits")
public class Fruit {
    @Id
    @Column(name = "fruit_id")
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long fruitId;

    @Column(nullable = false)
    private String emotion;

    @Column(nullable = false)
    private String todo;

    @Column(nullable = false)
    private LocalDate date;

    @Column(nullable = false)
    private LocalTime time;

    @Column(name = "accepted_at")
    private LocalDateTime acceptedAt;

    // JSONB 매핑
    @Column(columnDefinition = "jsonb")
    @JdbcTypeCode(JSON)
    private Position position;

    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;

    public Fruit() {}

    // 필요에 따라 All-args 생성자 추가...

    // getters & setters


    public Long getFruitId() {
        return fruitId;
    }

    public void setFruitId(Long fruitId) {
        this.fruitId = fruitId;
    }

    public String getEmotion() { return emotion; }
    public void setEmotion(String emotion) { this.emotion = emotion; }

    public String getTodo() { return todo; }
    public void setTodo(String todo) { this.todo = todo; }

    public LocalDate getDate() { return date; }
    public void setDate(LocalDate date) { this.date = date; }

    public LocalTime getTime() { return time; }
    public void setTime(LocalTime time) { this.time = time; }

    public LocalDateTime getAcceptedAt() { return acceptedAt; }
    public void setAcceptedAt(LocalDateTime acceptedAt) { this.acceptedAt = acceptedAt; }

    public Position getPosition() { return position; }
    public void setPosition(Position position) { this.position = position; }

    public LocalDateTime getCreatedAt() { return createdAt; }
    public void setCreatedAt(LocalDateTime createdAt) { this.createdAt = createdAt; }
} 