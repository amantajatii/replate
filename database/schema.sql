-- PostgreSQL 15+. Jalankan pada database replate yang masih kosong.
-- Acuan: ../erd-replate.mmd. Nama kolom menggunakan snake_case.
BEGIN;

CREATE TABLE users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name text NOT NULL,
    email text NOT NULL UNIQUE,
    password_hash text NOT NULL,
    role text NOT NULL CHECK (role IN ('CUSTOMER', 'RESTAURANT_OWNER')),
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE restaurants (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id uuid NOT NULL REFERENCES users(id),
    name text NOT NULL,
    description text NOT NULL,
    address text NOT NULL,
    phone text NOT NULL,
    opening_time time NOT NULL,
    closing_time time NOT NULL,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE menus (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    restaurant_id uuid NOT NULL REFERENCES restaurants(id),
    name text NOT NULL,
    description text NOT NULL,
    category text NOT NULL,
    normal_price numeric(14,2) NOT NULL CHECK (normal_price >= 0),
    weight_per_portion numeric(12,3) NOT NULL CHECK (weight_per_portion >= 0),
    image_url text NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE production_records (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    menu_id uuid NOT NULL REFERENCES menus(id),
    record_date date NOT NULL,
    produced_quantity integer NOT NULL CHECK (produced_quantity >= 0),
    regular_sold_quantity integer NOT NULL CHECK (regular_sold_quantity >= 0),
    surplus_quantity integer NOT NULL CHECK (surplus_quantity >= 0),
    rescued_quantity integer NOT NULL CHECK (rescued_quantity >= 0),
    wasted_quantity integer NOT NULL CHECK (wasted_quantity >= 0),
    UNIQUE (menu_id, record_date)
);

CREATE TABLE production_recommendations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    menu_id uuid NOT NULL REFERENCES menus(id),
    menu_name text NOT NULL,
    recommendation_date date NOT NULL,
    typical_production integer NOT NULL CHECK (typical_production >= 0),
    average_demand numeric(14,2) NOT NULL CHECK (average_demand >= 0),
    safety_buffer numeric(14,2) NOT NULL CHECK (safety_buffer >= 0),
    recommended_production integer NOT NULL CHECK (recommended_production >= 0),
    potential_surplus_reduction integer NOT NULL CHECK (potential_surplus_reduction >= 0),
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (menu_id, recommendation_date)
);

CREATE TABLE surplus_listings (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    menu_id uuid NOT NULL REFERENCES menus(id),
    rescue_price numeric(14,2) NOT NULL CHECK (rescue_price >= 0),
    quantity integer NOT NULL CHECK (quantity >= 0),
    pickup_start timestamptz NOT NULL,
    pickup_end timestamptz NOT NULL,
    status text NOT NULL DEFAULT 'ACTIVE'
        CHECK (status IN ('ACTIVE', 'SOLD_OUT', 'EXPIRED', 'CANCELLED')),
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHECK (pickup_end > pickup_start)
);

CREATE TABLE carts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id uuid NOT NULL UNIQUE REFERENCES users(id),
    restaurant_id uuid REFERENCES restaurants(id),
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE cart_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cart_id uuid NOT NULL REFERENCES carts(id),
    listing_id uuid NOT NULL REFERENCES surplus_listings(id),
    quantity integer NOT NULL CHECK (quantity > 0),
    unit_price numeric(14,2) NOT NULL CHECK (unit_price >= 0),
    UNIQUE (cart_id, listing_id)
);

CREATE TABLE orders (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id uuid NOT NULL REFERENCES users(id),
    restaurant_id uuid NOT NULL REFERENCES restaurants(id),
    pickup_code text NOT NULL UNIQUE,
    total_price numeric(14,2) NOT NULL CHECK (total_price >= 0),
    status text NOT NULL DEFAULT 'RESERVED'
        CHECK (status IN ('RESERVED', 'CONFIRMED', 'READY_FOR_PICKUP', 'COMPLETED', 'CANCELLED')),
    preparation_duration interval NOT NULL DEFAULT interval '0 seconds'
        CHECK (preparation_duration >= interval '0 seconds'),
    estimated_ready_time timestamptz NOT NULL,
    ordered_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at timestamptz
);

CREATE TABLE order_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id uuid NOT NULL REFERENCES orders(id),
    listing_id uuid NOT NULL REFERENCES surplus_listings(id),
    menu_name text NOT NULL,
    quantity integer NOT NULL CHECK (quantity > 0),
    unit_price numeric(14,2) NOT NULL CHECK (unit_price >= 0)
);

CREATE TABLE pickup_verifications (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id uuid NOT NULL UNIQUE REFERENCES orders(id),
    entered_code text NOT NULL,
    verified_at timestamptz,
    is_valid boolean NOT NULL DEFAULT false
);

COMMIT;
