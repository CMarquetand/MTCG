Create table users (id varchar primary key, 
					username varchar(50) unique, 
					password varchar(50),
				   coins integer,
				   elo integer,
				   bio varchar(250),
				   image varchar(50),
				   name varchar(50),
				   wins integer,
				losses integer,
				draws integer
);
Create table card (id varchar, 
				  name varchar(255), 
				   damage numeric(5,2),
				   package_id integer,
				   username varchar(50), 
				   primary key (id),
				   FOREIGN KEY (username) REFERENCES users(username));
Create table deck (user_id varchar NOT NULL, 
				   card_id varchar, 
				   primary key (user_id, card_id),
				   FOREIGN KEY (user_id) REFERENCES users(id),
				   FOREIGN KEY (card_id) REFERENCES card(id));
